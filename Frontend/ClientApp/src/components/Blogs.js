import React, { Component } from 'react';
import {withRouter} from 'react-router';
import './Blogs.css';
import { connect } from 'react-redux';
import { bindActionCreators } from 'redux';
import * as actions from '../actions/actions.js';
import PropTypes from 'prop-types';

class Blogs extends Component {
  displayName = Blogs.name

  constructor(props) {
    super(props);

  }

  componentWillMount() {
    if (!this.props.blogs || this.props.blogs.length === 0) {
      this.props.actions.loadBlogList();
    }
  }

  renderBlogsTable(blogs) {
    //var self = this;
    var rows = blogs.map(function(blog) {
        return (
          <tr key={blog.Name}>
            <td class='col'><a href={'/#/posts/' + blog.Name}>{blog.Name}</a></td>
            <td class='d-none d-md-table-cell'><a href={'/#/posts/' + blog.Name}>{blog.Title ? blog.Title : '\u00A0'}</a></td>
            <td class='d-none d-md-table-cell'><a href={'/#/posts/' + blog.Name}>{blog.Photo}</a></td>
            <td class='d-none d-md-table-cell'><a href={'/#/posts/' + blog.Name}>{blog.Video}</a></td>
            <td class='col'><a href={'/#/posts/' + blog.Name}>{blog.TotalPosts}</a></td>
          </tr>
        );}.bind(this));

    return (
      <table className='table'>
        <thead>
          <tr>
            <th>Name</th>
            <th class='d-none d-md-table-cell'>Title</th>
            <th class='d-none d-md-table-cell'>Photos</th>
            <th class='d-none d-md-table-cell'>Videos</th>
            <th>Total</th>
          </tr>
        </thead>
        <tbody className='blog-table'>
          {rows}
        </tbody>
      </table>
    );
  }

  render() {
    let contents = this.props.blogs.length === 0
      ? <p><em>Loading...</em></p>
        : this.renderBlogsTable(this.props.blogs);

    return (
      <div>
        <h1>Blogs available</h1>
        {contents}
      </div>
    );
  }
}

Blogs.propTypes = {
  actions: PropTypes.object,
  blogs: PropTypes.array
};

function mapStateToProps(state) {
  if (state.blogs.blogs) {
    return {
      blogs: state.blogs.blogs // weird hack, todo to find why there is that extra level
    };
  }
  return {
    blogs: state.blogs
  };
}

function mapDispatchToProps(dispatch) {
  return {
    actions: bindActionCreators(actions, dispatch)
  };
}

export default withRouter(connect(
  mapStateToProps,
  mapDispatchToProps
)(Blogs));
