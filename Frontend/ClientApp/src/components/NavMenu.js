import React, { Component } from 'react';
import { Link, HashRouter } from 'react-router-dom';
import { Route } from 'react-router';
import { Nav, Navbar } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import { MonthIndex } from './MonthIndex';
import './NavMenu.css';
import $ from 'jquery';

export class NavMenu extends Component {
  displayName = NavMenu.name

  componentDidMount() {
    $(function () {
      var lastScrollTop = 0;
      var $navbar = $('.navbar');

      $(window).scroll(function (event) { // make NavBar hidden when scrolling down and visible when scrolling up
        var st = $(this).scrollTop();

        if (st > lastScrollTop) { // scroll down
          $navbar.slideUp();
        } else { // scroll up
          $navbar.slideDown();
        }
        lastScrollTop = st;
      });
    });
  }

  render() {
    return (
      <HashRouter>
      <Navbar fixed='top' bg='light'>
        <Navbar.Brand>
          <Link to={'/'}>PicAI</Link>
        </Navbar.Brand>
        <Navbar.Toggle />
        <Navbar.Collapse>
          <Nav>
            <LinkContainer to={'/'} exact>
              <Nav.Link>Home</Nav.Link>
            </LinkContainer>
            <LinkContainer to={'/blogs'}>
              <Nav.Link>Blogs</Nav.Link>
            </LinkContainer>
          </Nav>
          </Navbar.Collapse>
          <Route path='/posts/:blogname' component={MonthIndex} />
        </Navbar>
        </HashRouter>
    );
  }
}
