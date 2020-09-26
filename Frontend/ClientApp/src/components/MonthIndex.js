import React, { Component } from 'react';
import Dropdown from "react-bootstrap/Dropdown";

export class MonthIndex extends Component {
  displayName = MonthIndex.name

  constructor(props) {
    super(props);
    this.state = { post: [], loading: true };

    fetch(process.env.REACT_APP_API_ROOT + '/api/monthindex/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ monthindex: data, loading: false });
    });
  }

  renderDropdown(monthindex) {
    return (
      <Dropdown>
        <Dropdown.Toggle variant="success" id="dropdown-basic">
          Month index
        </Dropdown.Toggle>

        <Dropdown.Menu>
        {monthindex.map(monthindex =>
          <Dropdown.Item href={ "#/posts/" + this.props.match.params.blogname + "/" + (monthindex.FirstPostId - 1) } key={monthindex.YearMonth}>
            {monthindex.YearMonth + " (" + monthindex.MonthsPosts + ")"}
          </Dropdown.Item>
        )}
        </Dropdown.Menu>
      </Dropdown>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderDropdown(this.state.monthindex);

    return <div>{contents}</div>
  }
}
